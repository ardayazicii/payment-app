const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:5228'

export interface BinInfo {
  bankName: string
  cardAssociation: string
  cardType: string
  cardFamily: string
}

export async function lookupBin(
  bin: string,
): Promise<BinInfo> {
  const response = await fetch(
    `${API_BASE_URL}/api/cards/bin-lookup`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ bin }),
    },
  )

  if (!response.ok) {
    const error = await response
      .json()
      .catch(() => null)

    throw new Error(
      error?.message ??
        'BIN sorgusu başarısız.',
    )
  }

  return await response.json()
}