export function getOrientation(orientation?: number): string {
  const map: Record<number, string> = {
    1: 'Normal',
    2: 'Flip horizontal',
    3: 'Rotate 180°',
    4: 'Flip vertical',
    5: 'Flip horizontal and rotate 270°',
    6: 'Rotate 90°',
    7: 'Flip horizontal and rotate 90°',
    8: 'Rotate 270°',
  };
  if (orientation === undefined) return 'unknown';
  return map[orientation] ?? `unknown (${orientation})`;
}
