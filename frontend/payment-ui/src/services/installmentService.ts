const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:5228'

export interface InstallmentOption {
  installmentNumber: number
  installmentPrice: number
  totalPrice: number
}

export interface InstallmentInfo {
  bankName: string
  cardAssociation: string
  cardFamily: string
  cardType: string
  installments: InstallmentOption[]
}

export async function lookupInstallments(
  bin: string,
  amount: number,
): Promise<InstallmentInfo> {
  const response = await fetch(
    `${API_BASE_URL}/api/cards/installments`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        bin,
        amount,
      }),
    },
  )

  if (!response.ok) {
    const error = await response
      .json()
      .catch(() => null)

    throw new Error(
      error?.message ??
        'Taksit bilgileri alınamadı.',
    )
  }

  return await response.json()
}