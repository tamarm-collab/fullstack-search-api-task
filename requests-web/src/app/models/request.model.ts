import { RequestStatus } from './request-status.enum';
import { RequestType } from './request-type.enum';

export interface RequestDto {
  id: number;
  requestNumber: string;
  customerId: number;
  ownerId: number;
  assignedToUserId: number | null;
  status: RequestStatus;
  requestType: RequestType;
  createdAt: string;
}
