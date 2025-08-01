﻿import {SystemEvent} from '@domain/DiagResponse';
import {CategoryModel} from './CategoryModel';
import {EventModel} from './EventModel';
import {FilterCriteria} from './FilterCriteria';

import pluralize from 'pluralize-esm';

export class EventSinkModel {
    name = '';
    events: EventModel[] = [];
    filteredEvents: EventModel[] = [];
    private latestReceived = 0;
    message = '';
    isExpanded = true;

    filterVisible = false;
    watchEnabled = false;

    filterCriteria = new FilterCriteria();

    constructor(readonly cat: CategoryModel, name: string) {
        this.watchEnabled = true;
        this.name = name;
    }

    public addEvents(evts: SystemEvent[]): void {

        // this.latestReceived = evt;

        const evtModels = evts.map(evt => new EventModel(evt));

        if (this.filterCriteria.isBlank) {
            this.events = [...evtModels, ...this.events];
        } else {
            this.events.unshift(...evtModels);
        }
        if (this.events.length > 500)
            this.events = this.events.slice(0, 500);

        this.filterEvents();
    }

    private onCriteriaChanged(): void {
        this.filterEvents();
    }

    private filterEvents(): void {
        if (this.filterCriteria.isBlank) {
            this.filteredEvents = this.events;
            this.message = pluralize('events', this.events.length, true);
        } else {
            this.filteredEvents = this.events.filter(evt => this.filterCriteria.filter(evt));
            this.message = `${this.filteredEvents.length} of ${this.events.length} ` + pluralize('event', this.events.length);
        }

        if (this.latestReceived)
            this.message += ` (+${this.latestReceived})`;
    }

    private onFilterVisibleChanged(): void {
        this.filterEvents();
    }

    handleDoubleClick(evt: MouseEvent) {
        if (evt.detail === 2) {
            this.isExpanded = true;
            this.cat.eventSinks.forEach(c => c.isExpanded = c === this);
            this.cat.subCats.forEach(c => c.isExpanded = false);
        }
    }
}

