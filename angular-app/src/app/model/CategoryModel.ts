import {EventResponse, PropertyBag, SystemEvent} from '@domain/DiagResponse';
import {customMerge} from '@util/merge';
import {EventSinkModel} from './EventSinkModel';
import {SubCat} from './SubCat';
import {RealtimeModel} from './RealtimeModel';
import * as _ from 'lodash-es';
import {Level} from './Level';
import {strEqCI} from '@util/stringUtil';
import {Signal, signal, WritableSignal} from "@angular/core";

export class CategoryModel {
    name = signal('');
    // propData = signal<PropertyBag[]>([]);
    eventData: EventResponse[] = [];
    subCats = signal<SubCat[]>([]);
    eventSinks: EventSinkModel[] = [];
    realtimeModel: RealtimeModel;
    labelClass = '';
    worstSev = 0;
    worstSevDate = new Date();

    constructor(realtimeModel: RealtimeModel, name: string, props: PropertyBag[] = []) {
        this.realtimeModel = realtimeModel;
        this.name.set(name);
        if (props)
            this.update(props);
    }

    update(props: PropertyBag[]) {
        // this.propData.set(props);
        this.subCats.set(customMerge(props,
            this.subCats(),
            s => s.name,
            t => t.name(),
            s => new SubCat(this, s),
            (s, t) => t.update(s)));
    }

    getSink(name: string): EventSinkModel {
        let sink = this.eventSinks.find(c => strEqCI(c.name, name));
        if (!sink)
            this.eventSinks.push(sink = new EventSinkModel(this, name));

        return sink;
    }
        
    expandCollapse(): void {
        console.log('expandCollapse', this.name())
        const expandable: { isCollapsed: WritableSignal<boolean> }[] = [];
        expandable.push(...this.subCats());
        // expandable.push(...this.eventSinks);

        const allExpanded = expandable.every(item => !item.isCollapsed());
        expandable.forEach(exp => exp.isCollapsed.set(allExpanded));
    }

    addEvents(evts: SystemEvent[]) {
        const maxLevel = _.maxBy(evts, evt => evt.level)?.level ?? 0;

        if (maxLevel >= this.worstSev) {
            this.worstSev = maxLevel;
            this.worstSevDate = new Date();
            this.labelClass = this.worstSev === 0 ? '' : 'event-level-' + Level.LevelToString(this.worstSev).toLocaleLowerCase();
        }

        const grouped = _.groupBy(evts, evt => evt.sinkName)
        for (const sinkName in grouped)
            this.getSink(sinkName).addEvents(grouped[sinkName]);
    }
    
    clearEvents() {
        for (let sink of this.eventSinks) {
            sink.events = [];
        }            
    }


    checkEventSeverityLevels() {
        if (this.worstSev > 0) {
            const time = new Date().valueOf() - this.worstSevDate.valueOf();

            if (time > 2_000) {
                this.worstSev = 0;
                this.labelClass = '';
            }
        }
    }
}
