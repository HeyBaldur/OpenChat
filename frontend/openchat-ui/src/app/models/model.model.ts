export interface OllamaModel {
  name: string;
  displayName: string;
  sizeBytes: number;
  sizeFormatted: string;
  family: string;
  parameterSize: string;
  supportsToolCalling: boolean;
}
