interface QRCodeImageProps {
  base64: string
  tag: string
  className?: string
}

export function QRCodeImage({ base64, tag, className = 'h-40 w-40' }: QRCodeImageProps) {
  return (
    <img
      src={`data:image/png;base64,${base64}`}
      alt={`QR code for ${tag}`}
      className={`${className} rounded-lg border border-slate-200 bg-white p-1`}
    />
  )
}
