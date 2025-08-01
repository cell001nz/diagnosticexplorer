import {DiagnosticResponse, OperationSet, PropertyBag} from '@domain/DiagResponse';
import * as _ from 'lodash-es';
import {inject, Injectable} from '@angular/core';
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
    titleMessage = '';
    categories: CategoryModel[] = [];
    operationSets: OperationSet[] = [];
    serverDate: Date | string | undefined;
    activeCat?: CategoryModel;
    selectedIndex = 0;

    public update(response: DiagnosticResponse) {
        this.titleMessage = 'Received at ' + format(new Date(), 'HH:mm:ss');
        this.serverDate = response.serverDate;

        const bagCats: { [key: string]: PropertyBag[] }
            = _.groupBy(response.propertyBags, p => p.category);

        const catData: { name: string, props: PropertyBag[] }[]
            = _.uniq(_.keys(bagCats).concat(this.categories.map(c => c.name)))
            .map(name => ({name, props: bagCats[name] ?? []}));

        let cats = this.categories.slice();

        customMerge(catData,
            cats,
            d => d.name,
            c => c.name,
            d => new CategoryModel(this, d.name, d.props),
            (d, c) => c.update(d.props),
            false);

        cats = _.sortBy(cats, c => c.name);

        if (cats.filter(c => !c.subCats.length && !c.eventSinks.length))
            cats = cats.filter(c => c.subCats.length || c.eventSinks.length);

        this.categories = cats;
        this.operationSets = response.operationSets;
    }
    dispose(): void {}
    disposed$ = new Subject<true>();
    //region process list



}
