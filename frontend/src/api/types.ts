export interface Employee {
  id: number;
  fullName: string;
  hireDate: string; // ISO yyyy-MM-dd
  color: string; // HEX, например #1677ff
  projects: string[];
  email: string | null;
  keycloakUserId: string | null;
}

export interface EmployeeUpsertRequest {
  fullName: string;
  hireDate: string;
  color: string;
  projects: string[];
  email: string | null;
  keycloakUserId: string | null;
}

export interface Vacation {
  id: number;
  employeeId: number;
  startDate: string; // ISO yyyy-MM-dd
  endDate: string; // ISO yyyy-MM-dd
  days: number;
}

export interface VacationUpsertRequest {
  employeeId: number;
  startDate: string;
  endDate: string;
}

export interface Project {
  id: number;
  name: string;
}

/** Кто сейчас залогинен: менеджер (видит/редактирует всё) или обычный сотрудник (только себя). */
export interface Me {
  isManager: boolean;
  employeeId: number | null;
}
