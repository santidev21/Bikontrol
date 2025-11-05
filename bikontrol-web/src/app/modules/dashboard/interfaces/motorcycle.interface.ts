export interface Motorcycle {
  id?: string;
  name: string;
  brand: string;
  year: number;
  nickname: string;
  km: number;
  image?: string;
  displacement: number;
  plate: string;
  isEnabled: boolean;
}

export interface CreateMotorcycleDTO {
  name: string;
  brand: string;
  year: number;
  nickname: string;
  km: number;
  displacement: number;
  plate: string;
}

export interface UpdateMotorcycleDTO {
  name: string;
  brand: string;
  year: number;
  nickname: string;
  km: number;
  displacement: number;
  plate: string;
}