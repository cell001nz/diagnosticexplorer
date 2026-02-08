import {Property} from '@domain/DiagResponse';
import {PropGroupModel} from './PropGroupModel';
import {signal} from "@angular/core";

export class PropModel {
    group: PropGroupModel;
    name = signal('');
    value = signal('');
    description = signal('');
    operationSet = signal('');
    canSet = signal(false);

    constructor(group: PropGroupModel, source: Property) {
        this.group = group;
        this.name.set(source.name);
        this.update(source);
    }

    update(source: Property): void {
        this.value.set(source.value);
        this.description.set(source.description);
        this.operationSet.set(source.operationSet);
        this.canSet.set(source.canSet);
    }

    getPropertyPath(): string {
        const pathElements = [
            this.group.subCat.cat.name(),
            this.group.subCat.name(),
            this.group.name(),
            this.name()];

        return pathElements.join('|');
    }
}
