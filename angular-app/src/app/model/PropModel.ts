import {Property} from '@domain/DiagResponse';
import {PropGroup} from './PropGroup';

export class PropModel {
    group: PropGroup;
    name = '';
    value = '';
    description = '';
    operationSet = '';
    canSet = false;

    constructor(group: PropGroup, source: Property) {
        this.group = group;
        this.name = source.name;
        this.update(source);
    }

    update(source: Property): void {
        this.value = source.value;
        this.description = source.description;
        this.operationSet = source.operationSet;
        this.canSet = source.canSet;
    }

    getPropertyPath(): string {
        const pathElements = [
            this.group.subCat.cat.name,
            this.group.subCat.name,
            this.group.name,
            this.name];

        return pathElements.join('|');
    }
}
