import { ClariveCircuitOpenError } from "./errors.js";

type State = "closed" | "open" | "half_open";

export class CircuitBreaker {
  private state: State = "closed";
  private failureCount = 0;
  private openedAt: number | null = null;

  constructor(
    private readonly threshold: number,
    private readonly cooldown: number,
  ) {}

  get currentState(): State {
    return this.state;
  }

  check(): void {
    if (this.state === "closed") return;

    if (this.state === "half_open") return;

    // Open — check if cooldown expired
    const elapsed = (Date.now() - (this.openedAt ?? 0)) / 1000;
    if (elapsed >= this.cooldown) {
      this.state = "half_open";
      return;
    }

    throw new ClariveCircuitOpenError();
  }

  recordSuccess(): void {
    this.failureCount = 0;
    this.state = "closed";
    this.openedAt = null;
  }

  recordFailure(): void {
    this.failureCount++;
    if (this.failureCount >= this.threshold) {
      this.state = "open";
      this.openedAt = Date.now();
    }
  }

  reset(): void {
    this.failureCount = 0;
    this.state = "closed";
    this.openedAt = null;
  }
}
