import {signal} from "@angular/core";
import {PropertyBag} from "@domain/DiagResponse";

export class PropertyBagModel {
  name = signal('');
  category = signal('');
  operationSet = signal('');
  categories = signal<CategoryModel[]>([]);

  constructor(propertyBag?: PropertyBag) {
    if (propertyBag) {
      this.update(propertyBag);
    }
  }

  update(propertyBag: PropertyBag) {
    this.name.set(propertyBag.name);
    this.category.set(propertyBag.category);
    this.operationSet.set(propertyBag.operationSet);
    this.categories.set(propertyBag.categories);
  }
}