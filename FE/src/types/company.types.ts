export interface Company {
  id: string;
  name: string;
  description?: string;
}

export interface Department {
  id: string;
  name: string;
  companyId: string;
  companyName?: string;
}

export interface Position {
  id: string;
  name: string;
  requiresCertificate: boolean;
  certificateName?: string;
}

export interface CompanyPosition {
  id: string;
  companyId: string;
  companyName?: string;
  positionId: string;
  positionName?: string;
  isActive: boolean;
}
