/**
 * Modelos de domínio compartilhados pelos clientes JS.
 * Espelham os contratos da API ASP.NET Core (backend/).
 */

export type Id = string;
export type IsoDateTime = string;

export type TaskPriority = 1 | 2 | 3 | 4;

/** Projeto = lista com cor. */
export interface Project {
  id: Id;
  name: string;
  color: string;
  createdAt: IsoDateTime;
  updatedAt: IsoDateTime;
}

/** Categoria = etiqueta transversal (@etiqueta). */
export interface Category {
  id: Id;
  name: string;
  createdAt: IsoDateTime;
  updatedAt: IsoDateTime;
}

export interface Task {
  id: Id;
  title: string;
  notes?: string;
  priority: TaskPriority;
  projectId?: Id;
  categoryIds: Id[];
  dueDate?: IsoDateTime;
  completedAt?: IsoDateTime;
  createdAt: IsoDateTime;
  updatedAt: IsoDateTime;
}

/** Listas inteligentes. Não existe Entrada: tarefa sem data cai em Hoje. */
export type SmartList = "today" | "upcoming" | "all";
