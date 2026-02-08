export interface DiagnosticResponse {
    processId: string;
    propertyBags: PropertyBag[];
    events: EventResponse[];
    operationSets: OperationSet[];
    context?: string;
    exceptionMessage?: string;
    exceptionDetail?: string;
    date: Date | string;
    serverDate: Date | string;
}

export interface PropertyBag {
    name: string;
    category: string;
    operationSet: string;
    categories: Category[];
}

export interface Category {
  name: string;
  operationSet: string;
  properties: Property[];
}

export interface Property {
  name: string;
  value: string;
  description: string;
  operationSet: string;
  canSet: boolean;
}

export interface EventResponse {
  name: string;
  category: string;
  events: SystemEvent[];
}

export interface SystemEvent {
  id: number;
  date: string | Date;
  message: string;
  detail: string;
  level: number;
  severity: string;
  sinkName: string;
  sinkCategory: string;
}
 
export interface OperationSet {
  id: string;
  operations: Operation[];
}

export interface Operation { 
  returnType: string;
  signature: string;
  description: string;
  parameters: OperationParameter[];
}

export interface OperationParameter {
  name: string;
  type: string;
}