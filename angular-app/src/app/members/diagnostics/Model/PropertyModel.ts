import {signal} from "@angular/core";
import {Property} from "@domain/DiagResponse";

export class PropertyModel {
  name = signal('');
  value = signal('');
  description = signal('');
  operationSet = signal('');
  canSet = signal(false);

  constructor(property?: Property) {
    if (property) {
      this.update(property);
    }
  }

  update(property: Property) {
    this.name.set(property.name);
    this.value.set(property.value);
    this.description.set(property.description);
    this.operationSet.set(property.operationSet);
    this.canSet.set(property.canSet);
  }
}