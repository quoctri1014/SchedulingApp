export const ALGORITHM_NAMES = {
  GREEDY: 'greedy',
  GA: 'ga',
  SA: 'sa',
  HYBRID: 'hybrid',
} as const;

export type AlgorithmType = typeof ALGORITHM_NAMES[keyof typeof ALGORITHM_NAMES];
