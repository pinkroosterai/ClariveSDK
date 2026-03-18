export interface ResilienceOptions {
  enabled?: boolean;
  maxRetries?: number;
  baseDelay?: number;
  timeout?: number;
  circuitBreakerThreshold?: number;
  circuitBreakerCooldown?: number;
}

export const DEFAULT_RESILIENCE_OPTIONS: Required<ResilienceOptions> = {
  enabled: true,
  maxRetries: 3,
  baseDelay: 1,
  timeout: 30,
  circuitBreakerThreshold: 5,
  circuitBreakerCooldown: 30,
};
