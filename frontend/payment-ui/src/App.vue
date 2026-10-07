<script setup lang="ts">
import { ref, watch } from 'vue'

import {
  lookupBin,
  type BinInfo,
} from './services/binService'

import {
  lookupInstallments,
  type InstallmentInfo,
} from './services/installmentService'

import {
  start3dsPayment,
} from './services/paymentService'

const amount = ref('')
const cardHolder = ref('')
const cardNumber = ref('')
const expiry = ref('')
const cvv = ref('')

const binInfo = ref<BinInfo | null>(null)
const binLoading = ref(false)
const binError = ref('')

const installmentInfo =
  ref<InstallmentInfo | null>(null)

const installmentLoading = ref(false)
const installmentError = ref('')

const selectedInstallment = ref(1)

const paymentLoading = ref(false)
const paymentError = ref('')

let lastBin = ''

let installmentTimer:
  | ReturnType<typeof setTimeout>
  | undefined

function digitsOnly(value: string) {
  return value.replace(/\D/g, '')
}

function formatCardNumber(value: string) {
  return digitsOnly(value)
    .slice(0, 16)
    .replace(/(.{4})/g, '$1 ')
    .trim()
}

function handleCardInput(event: Event) {
  const input =
    event.target as HTMLInputElement

  cardNumber.value =
    formatCardNumber(input.value)
}

function formatExpiry(value: string) {
  const digits =
    digitsOnly(value).slice(0, 4)

  if (digits.length <= 2) {
    return digits
  }

  return `${digits.slice(0, 2)}/${digits.slice(2)}`
}

function handleExpiryInput(event: Event) {
  const input =
    event.target as HTMLInputElement

  expiry.value =
    formatExpiry(input.value)
}

function handleCvvInput(event: Event) {
  const input =
    event.target as HTMLInputElement

  cvv.value =
    digitsOnly(input.value).slice(0, 4)
}

watch(cardNumber, async (value) => {
  const cleanCardNumber =
    digitsOnly(value)

  if (cleanCardNumber.length < 8) {
    binInfo.value = null
    binError.value = ''
    lastBin = ''
    return
  }

  const bin =
    cleanCardNumber.slice(0, 8)

  if (bin === lastBin) {
    return
  }

  lastBin = bin

  binLoading.value = true
  binError.value = ''

  try {
    binInfo.value =
      await lookupBin(bin)
  } catch {
    binInfo.value = null

    binError.value =
      'Kart bilgisi alınamadı.'
  } finally {
    binLoading.value = false
  }
})

watch(
  [cardNumber, amount],
  ([cardValue, amountValue]) => {
    if (installmentTimer) {
      clearTimeout(installmentTimer)
    }

    const cleanCardNumber =
      digitsOnly(cardValue)

    const numericAmount =
      Number(amountValue)

    if (
      cleanCardNumber.length < 8 ||
      !numericAmount ||
      numericAmount <= 0
    ) {
      installmentInfo.value = null
      installmentError.value = ''
      selectedInstallment.value = 1
      return
    }

    const bin =
      cleanCardNumber.slice(0, 8)

    installmentLoading.value = true
    installmentError.value = ''

    // Kullanıcı tutarı yazarken her tuşta API çağrısı yapmayalım.
    installmentTimer = setTimeout(
      async () => {
        try {
          const result =
            await lookupInstallments(
              bin,
              numericAmount,
            )

          installmentInfo.value =
            result

          const singlePayment =
            result.installments.find(
              x =>
                x.installmentNumber === 1,
            )

          selectedInstallment.value =
            singlePayment
              ? 1
              : result.installments[0]
                    ?.installmentNumber ?? 1
        } catch (error) {
          installmentInfo.value = null

          installmentError.value =
            error instanceof Error
              ? error.message
              : 'Taksit bilgileri alınamadı.'
        } finally {
          installmentLoading.value = false
        }
      },
      400,
    )
  },
)

async function startPayment() {
  paymentError.value = ''

  const cleanCardNumber =
    digitsOnly(cardNumber.value)

  const [
    expireMonth,
    expireYearShort,
  ] = expiry.value.split('/')

  if (
    !amount.value ||
    Number(amount.value) <= 0 ||
    !cardHolder.value.trim() ||
    cleanCardNumber.length !== 16 ||
    !expireMonth ||
    !expireYearShort ||
    expireMonth.length !== 2 ||
    expireYearShort.length !== 2 ||
    Number(expireMonth) < 1 ||
    Number(expireMonth) > 12 ||
    cvv.value.length < 3
  ) {
    paymentError.value =
      'Lütfen tüm kart bilgilerini doğru şekilde doldurun.'

    return
  }

  const expireYear =
    expireYearShort.length === 2
      ? `20${expireYearShort}`
      : expireYearShort

  const popup =
    window.open('', '_blank')

  if (!popup) {
    paymentError.value =
      '3D Secure penceresi açılamadı. Popup engelleyiciyi kontrol edin.'

    return
  }

  popup.document.write(`
    <html>
      <body style="
        font-family: Arial, sans-serif;
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
      ">
        <p>3D Secure hazırlanıyor...</p>
      </body>
    </html>
  `)

  paymentLoading.value = true

  try {
    const result =
      await start3dsPayment({
        amount:
          Number(amount.value),

        cardHolderName:
          cardHolder.value.trim(),

        cardNumber:
          cleanCardNumber,

        expireMonth,

        expireYear,

        cvc:
          cvv.value,

        installment:
          selectedInstallment.value,
      })

    popup.document.open()

    popup.document.write(
      result.htmlContent,
    )

    popup.document.close()
  } catch (error) {
    popup.close()

    paymentError.value =
      error instanceof Error
        ? error.message
        : 'Ödeme başlatılamadı.'
  } finally {
    paymentLoading.value = false
  }
}

function formatMoney(
  value: number,
) {
  return new Intl.NumberFormat(
    'tr-TR',
    {
      style: 'currency',
      currency: 'TRY',
    },
  ).format(value)
}
</script>

<template>
  <main class="page">
    <div class="payment-card">
      <h1>Ödeme</h1>

      <p class="subtitle">
        3D Secure ile güvenli ödeme
      </p>

      <label>
        Tutar

        <input
          v-model="amount"
          type="number"
          min="0"
          step="0.01"
          placeholder="1250.00"
        />
      </label>

      <label>
        Kart üzerindeki isim

        <input
          v-model="cardHolder"
          placeholder="Ad Soyad"
        />
      </label>

      <label>
        Kart numarası

        <input
          :value="cardNumber"
          inputmode="numeric"
          maxlength="19"
          placeholder="0000 0000 0000 0000"
          autocomplete="cc-number"
          @input="handleCardInput"
        />
      </label>

      <!-- BIN bilgileri -->
      <div
        v-if="binLoading"
        class="card-info"
      >
        Kart bilgileri sorgulanıyor...
      </div>

      <div
        v-else-if="binInfo"
        class="card-info"
      >
        <strong>
          {{ binInfo.cardAssociation }}
        </strong>

        <span>
          {{ binInfo.bankName }}
        </span>

        <span v-if="binInfo.cardFamily">
          {{ binInfo.cardFamily }}
        </span>

        <span>
          {{ binInfo.cardType }}
        </span>
      </div>

      <div
        v-if="binError"
        class="error"
      >
        {{ binError }}
      </div>

      <!-- Taksitler -->
      <div
        v-if="installmentLoading"
        class="installment-loading"
      >
        Taksit seçenekleri hesaplanıyor...
      </div>

      <div
        v-else-if="installmentInfo"
        class="installments"
      >
        <div class="installment-title">
          Taksit seçenekleri
        </div>

        <label
          v-for="option in installmentInfo.installments"
          :key="option.installmentNumber"
          class="installment-option"
        >
          <input
            v-model="selectedInstallment"
            type="radio"
            name="installment"
            :value="option.installmentNumber"
          />

          <div class="installment-text">
            <strong>
              {{
                option.installmentNumber === 1
                  ? 'Tek çekim'
                  : `${option.installmentNumber} taksit`
              }}
            </strong>

            <span
              v-if="option.installmentNumber === 1"
            >
              {{ formatMoney(option.totalPrice) }}
            </span>

            <span v-else>
              {{ option.installmentNumber }}
              ×
              {{
                formatMoney(
                  option.installmentPrice,
                )
              }}

              · Toplam

              {{
                formatMoney(
                  option.totalPrice,
                )
              }}
            </span>
          </div>
        </label>
      </div>

      <div
        v-if="installmentError"
        class="error"
      >
        {{ installmentError }}
      </div>

      <div class="row">
        <label>
          Son kullanma

          <input
            :value="expiry"
            inputmode="numeric"
            maxlength="5"
            placeholder="AA/YY"
            autocomplete="cc-exp"
            @input="handleExpiryInput"
          />
        </label>

        <label>
          CVV

          <input
            :value="cvv"
            type="password"
            inputmode="numeric"
            maxlength="4"
            placeholder="***"
            autocomplete="cc-csc"
            @input="handleCvvInput"
          />
        </label>
      </div>

      <button
        type="button"
        :disabled="
          paymentLoading ||
          installmentLoading
        "
        @click="startPayment"
      >
        {{
          paymentLoading
            ? '3D Secure başlatılıyor...'
            : '3D Secure ile Öde'
        }}
      </button>

      <div
        v-if="paymentError"
        class="error payment-error"
      >
        {{ paymentError }}
      </div>
    </div>
  </main>
</template>

<style scoped>
.page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  background: #f4f5f7;
  font-family: Inter, Arial, sans-serif;
}

.payment-card {
  width: min(460px, calc(100vw - 40px));
  background: white;
  padding: 32px;
  border-radius: 20px;
  box-shadow: 0 18px 50px rgba(0, 0, 0, 0.08);
}

h1 {
  margin: 0;
}

.subtitle {
  margin-top: 6px;
  margin-bottom: 28px;
  opacity: 0.6;
}

label {
  display: flex;
  flex-direction: column;
  gap: 7px;
  margin-bottom: 18px;
  font-size: 14px;
  font-weight: 600;
}

input {
  height: 46px;
  padding: 0 14px;
  border: 1px solid #d8dbe0;
  border-radius: 10px;
  font-size: 15px;
}

input:focus {
  outline: none;
  border-color: #555;
}

.row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.card-info {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  align-items: center;
  margin: -8px 0 20px;
  padding: 12px;
  background: #f5f6f7;
  border-radius: 10px;
  font-size: 13px;
}

.installment-loading {
  margin: -4px 0 20px;
  padding: 12px;
  background: #f5f6f7;
  border-radius: 10px;
  font-size: 13px;
}

.installments {
  margin-bottom: 20px;
  border: 1px solid #e2e4e8;
  border-radius: 12px;
  overflow: hidden;
}

.installment-title {
  padding: 12px 14px;
  font-size: 13px;
  font-weight: 700;
  background: #f5f6f7;
}

.installment-option {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 12px;
  margin: 0;
  padding: 12px 14px;
  border-top: 1px solid #eeeeee;
  cursor: pointer;
}

.installment-option input {
  width: 18px;
  height: 18px;
  margin: 0;
  padding: 0;
}

.installment-text {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.installment-text span {
  font-size: 12px;
  font-weight: 400;
  opacity: 0.7;
}

.error {
  margin: -8px 0 20px;
  font-size: 13px;
  color: #b42318;
}

.payment-error {
  margin-top: 14px;
  margin-bottom: 0;
}

button {
  width: 100%;
  height: 48px;
  border: 0;
  border-radius: 11px;
  font-size: 15px;
  font-weight: 700;
  cursor: pointer;
}

button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
</style>
