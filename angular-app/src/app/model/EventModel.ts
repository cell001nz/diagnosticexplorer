import {IFilterableEvent} from './IFilterableEvent';
import {ScopeNode} from "./ScopeNode";
import {SystemEvent} from "@domain/DiagResponse";

export class EventModel implements SystemEvent, IFilterableEvent {
    isSelected = false;
    machine = '';
    process = '';
    user = '';
    id: number;
    date: string | Date;
    message: string;
    detail: string;
    level: number;
    severity = '';
    sinkName = '';
    sinkCategory = '';

    region?: ScopeNode;

    constructor(evt: SystemEvent) {

        this.id = evt.id;
        this.date = evt.date;
        this.level = evt.level;
        this.message = evt.message;
        this.detail = evt.detail;

        this.region = ScopeNode.parseTraceScope(this.displayText);
    }

   
    get displayText(): string {
        return this.detail ?? this.message;
    }

}
