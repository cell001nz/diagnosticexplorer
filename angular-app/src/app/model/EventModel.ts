import {IFilterableEvent} from './IFilterableEvent';
import {ScopeNode} from "./ScopeNode";
import {SystemEvent} from "@domain/DiagResponse";

export class EventModel implements SystemEvent, IFilterableEvent {
    isSelected = false;
    machine = '';
    process = '';
    user = '';
    id: number;
    sinkSeq: number;
    date: string | Date;
    message: string;
    detail: string;
    level: number;
    severity = '';
    sink = '';
    cat = '';

    region?: ScopeNode;

    constructor(evt: SystemEvent) {

        this.id = evt.id;
        this.sinkSeq = evt.sinkSeq;
        this.date = evt.date;
        this.level = evt.level;
        this.message = evt.message;
        this.detail = evt.detail;
        this.sink = evt.sink;
        this.cat = evt.cat;
        this.severity = evt.severity;

        this.region = ScopeNode.parseTraceScope(this.displayText);
    }

   
    get displayText(): string {
        return this.detail ?? this.message;
    }

}
