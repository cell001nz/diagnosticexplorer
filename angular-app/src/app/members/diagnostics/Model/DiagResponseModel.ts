import {signal} from "@angular/core";
import {DiagnosticResponse, OperationSet} from "@domain/DiagResponse";
import {PropertyBagModel} from "@app/members/diagnostics/Model/PropertyBagModel";

export class DiagResponseModel {
  propertyBags = signal<PropertyBagModel[]>([]);
  // events = signal<EventResponseModel[]>([]);
  operationSets = signal<OperationSet[]>([]);
  context = signal<string | undefined>(undefined);
  exceptionMessage = signal<string | undefined>(undefined);
  exceptionDetail = signal<string | undefined>(undefined);
  date = signal<Date | string>(new Date());
  serverDate = signal<Date | string>(new Date());

  constructor(response?: DiagnosticResponse) {
    if (response) {
      this.update(response);
    }
  }

  update(response: DiagnosticResponse) {
    
    this.propertyBags.set(response.propertyBags);
    // this.events.set(response.events);
    this.operationSets.set(response.operationSets);
    this.context.set(response.context);
    this.exceptionMessage.set(response.exceptionMessage);
    this.exceptionDetail.set(response.exceptionDetail);
    this.date.set(response.date);
    this.serverDate.set(response.serverDate);
  }
}