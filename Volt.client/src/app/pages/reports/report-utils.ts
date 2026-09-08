import { FileResponse } from '../../core/services/clientAPI';

export function downloadFileResponse(file: FileResponse, fallbackName: string): void {
  const blob = file.data;
  const fileName = file.fileName || fallbackName;
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}

export function toNumberArray(values: Array<string | number | boolean>): number[] {
  return values
    .map(v => typeof v === 'number' ? v : Number(v))
    .filter(v => Number.isFinite(v));
}

export function optionalDate(value: string | null | undefined): Date | null {
  if (!value) return null;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function emptyToNull<T>(values: T[] | null | undefined): T[] | null {
  return values && values.length > 0 ? values : null;
}
