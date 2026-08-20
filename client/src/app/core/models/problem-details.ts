/**
 * RFC 7807 shape returned by every error from every backend endpoint
 * (Modular.Common.ErrorOrExtensions.ToResult / the global exception handler).
 * Only ever one error per response — never a per-field map. See
 * docs/frontend-prd.md Appendix A.
 */
export interface ProblemDetails {
  type?: string;
  title?: string | null;
  status?: number;
  detail?: string;
}

export const FALLBACK_ERROR_MESSAGE = 'Something went wrong. Please try again.';

export function extractErrorMessage(error: unknown): string {
  if (
    error &&
    typeof error === 'object' &&
    'error' in error &&
    error.error &&
    typeof error.error === 'object' &&
    'detail' in error.error &&
    typeof (error.error as ProblemDetails).detail === 'string'
  ) {
    return (error.error as ProblemDetails).detail as string;
  }
  return FALLBACK_ERROR_MESSAGE;
}
