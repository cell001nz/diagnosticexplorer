import {signal} from "@angular/core";
import {Category} from "@domain/DiagResponse";

export class CategoryModel {
  name = signal('');
  operationSet = signal('');
  properties = signal<PropertyModel[]>([]);

  constructor(category?: Category) {
    if (category) {
      this.update(category);
    }
  }

  update(category: Category) {
    this.name.set(category.name);
    this.operationSet.set(category.operationSet);
    this.properties.set(category.properties);
  }
}