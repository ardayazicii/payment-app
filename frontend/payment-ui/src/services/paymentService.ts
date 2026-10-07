export interface PaymentRequest {
  amount: number
  cardHolderName: string
  cardNumber: string
  expireMonth: string
  expireYear: string
  cvc: string
  installment: number
}

export interface PaymentStartResponse {
  paymentId: string
  htmlContent: string
}

export async function start3dsPayment(
  request: PaymentRequest,
): Promise<PaymentStartResponse> {
  const response = await fetch(
    'http://localhost:5228/api/payments/3ds/start',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    },
  )

  if (!response.ok) {
    const error = await response.json().catch(() => null)

    throw new Error(
      error?.message ?? '3D Secure ödeme başlatılamadı.',
    )
  }

  return await response.json()
}