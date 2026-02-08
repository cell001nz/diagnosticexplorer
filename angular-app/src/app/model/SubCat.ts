import {PropertyBag} from '@domain/DiagResponse';
import {customMerge} from '@util/merge';
import {PropGroupModel} from './PropGroupModel';
import {CategoryModel} from './CategoryModel';
import {computed, signal} from "@angular/core";

export class SubCat {
    cat: CategoryModel;
    name = signal('');
    groups = signal<PropGroupModel[]>([]);
    isCollapsed = signal(false);
    isExpanded = computed(() => !this.isCollapsed());
    
    operationSet = '';

    constructor(cat: CategoryModel, bag: PropertyBag) {
        this.cat = cat;
        this.name.set(bag.name);
        this.update(bag);
    }

    update(bag: PropertyBag) {
        this.operationSet = bag.operationSet;
        
        this.groups.set(customMerge(bag.categories,
            this.groups(),
            s => s.name ?? "General",
            t => t.name(),
            s => new PropGroupModel(this, s),
            (s, t) => t.update(s)));
    }

    handleDoubleClick(evt: MouseEvent) {
        if (evt.detail === 2) {
            this.isCollapsed.set(false);
            this.cat.subCats().forEach(c => c.isCollapsed.set(c !== this));
            this.cat.eventSinks.forEach(c => c.isExpanded = false);
        }
    }
}

