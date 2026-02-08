import {PropModel} from './PropModel';
import {Category} from '@domain/DiagResponse';
import {customMerge} from '@util/merge';
import {SubCat} from './SubCat';
import {signal} from "@angular/core";

export class PropGroupModel {
    subCat: SubCat;
    name = signal('');
    properties = signal<PropModel[]>([]);

    constructor(subCat: SubCat, propCat: Category) {
        this.subCat = subCat;
        this.name.set(propCat.name);
        this.update(propCat);
    }

    update(propCat: Category) {
        this.properties.set(customMerge(propCat.properties,
            this.properties(),
            s => s.name,
            t => t.name(),
            s => new PropModel(this, s),
            (s, t) => t.update(s)));
    }
}
