/**
 * Cliente HTTP da API do Orderly. Esqueleto: só o health check por enquanto.
 * Os endpoints de tarefas, projetos e auth entram na Fase 1.
 */

export interface OrderlyClientOptions {
  baseUrl: string;
  getAccessToken?: () => Promise<string | null> | string | null;
  fetch?: typeof fetch;
}

export interface HealthResponse {
  status: string;
}

export class OrderlyClient {
  private readonly baseUrl: string;
  private readonly fetchImpl: typeof fetch;

  constructor(private readonly options: OrderlyClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/+$/, "");
    this.fetchImpl = options.fetch ?? globalThis.fetch.bind(globalThis);
  }

  async health(): Promise<HealthResponse> {
    return this.request<HealthResponse>("/health");
  }

  private async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set("Accept", "application/json");
    const token = await this.options.getAccessToken?.();
    if (token) headers.set("Authorization", `Bearer ${token}`);

    const response = await this.fetchImpl(`${this.baseUrl}${path}`, { ...init, headers });
    if (!response.ok) {
      throw new Error(`Orderly API ${response.status} em ${path}`);
    }
    return (await response.json()) as T;
  }
}
