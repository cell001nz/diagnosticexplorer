import {SystemEvent} from '@domain/DiagResponse';
import {CategoryModel} from './CategoryModel';
import {EventModel} from './EventModel';
import {FilterCriteria} from './FilterCriteria';
import {signal} from '@angular/core';

import pluralize from 'pluralize-esm';

export class EventSinkModel {
    name = '';
    events = signal<EventModel[]>([]);
    filteredEvents = signal<EventModel[]>([]);
    private latestReceived = 0;
    message = '';
    isCollapsed = false

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
        const currentEvents = this.events();

        let newEvents: EventModel[];
        if (this.filterCriteria.isBlank) {
            newEvents = [...evtModels, ...currentEvents];
        } else {
            newEvents = [...evtModels, ...currentEvents];
        }
        if (newEvents.length > 500)
            newEvents = newEvents.slice(0, 500);

        this.events.set(newEvents);
        this.filterEvents();
    }

    public clearEvents(): void {
        this.events.set([]);
        this.filteredEvents.set([]);
        this.message = '';
    }

    private onCriteriaChanged(): void {
        this.filterEvents();
    }

    private filterEvents(): void {
        const currentEvents = this.events();
        if (this.filterCriteria.isBlank) {
            this.filteredEvents.set(currentEvents);
            this.message = pluralize('events', currentEvents.length, true);
        } else {
            const filtered = currentEvents.filter(evt => this.filterCriteria.filter(evt));
            this.filteredEvents.set(filtered);
            this.message = `${filtered.length} of ${currentEvents.length} ` + pluralize('event', currentEvents.length);
        }

        if (this.latestReceived)
            this.message += ` (+${this.latestReceived})`;
    }

    private onFilterVisibleChanged(): void {
        this.filterEvents();
    }

    handleDoubleClick(evt: MouseEvent) {
        if (evt.detail === 2) {
            this.isCollapsed = false;
            this.cat.eventSinks().forEach(c => c.isCollapsed = c !== this);
            this.cat.subCats().forEach(c => c.isCollapsed.set(true));
        }
    }
}
