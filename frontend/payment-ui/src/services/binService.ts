export interface BinInfo {
  bankName: string
  cardAssociation: string
  cardType: string
  cardFamily: string
}

export async function lookupBin(bin: string): Promise<BinInfo> {
  const response = await fetch(
    'http://localhost:5228/api/cards/bin-lookup',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ bin }),
    },
  )

  if (!response.ok) {
    throw new Error('BIN sorgusu başarısız.')
  }

  return await response.json()
}