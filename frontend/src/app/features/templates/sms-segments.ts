export interface SmsSegmentStats {
  length: number;
  segmentSize: number;
  segments: number;
  encoding: string;
}

export function calculateSmsSegments(text: string): SmsSegmentStats {
  const length = Array.from(text).length;
  const isUnicode = /[^\u0000-\u007f]/.test(text);
  const segmentSize = isUnicode ? 70 : 160;

  return {
    length,
    segmentSize,
    segments: Math.max(1, Math.ceil(length / segmentSize)),
    encoding: isUnicode ? 'יוניקוד' : 'לטיני',
  };
}
