import {DiagnosticResponse, OperationSet, PropertyBag} from '@domain/DiagResponse';
import * as _ from 'lodash-es';
import {computed, inject, Injectable, signal} from '@angular/core';
import {CategoryModel} from './CategoryModel';
import {EventModel} from './EventModel';
import {customMerge} from "@util/merge";
import {format} from "date-fns";
import {ObservableDisposable} from "@model/ObservableDisposable";
import {Subject} from "rxjs";
import {DiagnosticModelFactory} from "@model/DiagnosticModelFactory";

@Injectable({providedIn: 'root'})
export class RealtimeModel implements ObservableDisposable {

    #modelFactory = inject(DiagnosticModelFactory);
    selectedEvent?: EventModel;
    titleMessage = signal('');
    categories = signal<CategoryModel[]>([]);
    operationSets = signal<OperationSet[]>([]);
    serverDate = signal<Date | undefined>(undefined);
    activeCatName = signal('');
    activeCat = computed(() => this.categories().find(c => c.name() === this.activeCatName()));

    clear() {
        this.titleMessage.set("...");
        this.categories.set([]);
        this.operationSets.set([]);
        this.serverDate.set(undefined);
        this.activeCatName.set('');
    }
    
    public update(response: DiagnosticResponse) {
        this.titleMessage.set('Received ' + format(new Date(), 'HH:mm:ss'));
        this.serverDate.set(new Date(response.serverDate));

        const bagCats: { [key: string]: PropertyBag[] }
            = _.groupBy(response.propertyBags, p => p.category);

        const catData: { name: string, props: PropertyBag[] }[]
            = _.uniq(_.keys(bagCats).concat(this.categories().map(c => c.name())))
            .map(name => ({name, props: bagCats[name] ?? []}));

        let cats = this.categories().slice();

        customMerge(catData,
            cats,
            d => d.name,
            c => c.name(),
            d => new CategoryModel(this, d.name, d.props),
            (d, c) => c.update(d.props),
            false);

        cats = _.sortBy(cats, c => c.name);
            
        if (cats.filter(c => !c.subCats().length && !c.eventSinks.length))
            cats = cats.filter(c => c.subCats().length || c.eventSinks.length);

        this.categories.set(cats);
        this.operationSets.set(response.operationSets);
        
        if (!this.activeCatName() && this.categories().length)
            this.activeCatName.set(this.categories()[0].name());        
    }
    dispose(): void {}
    disposed$ = new Subject<true>();
    //region process list



}
