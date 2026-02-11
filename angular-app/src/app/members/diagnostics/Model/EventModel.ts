import {signal} from "@angular/core";
import {SystemEvent} from "@domain/DiagResponse";

export class SystemEventModel {
  id = signal(0);
  date = signal(new Date());
  message = signal('');
  detail = signal('');
  level = signal('');
  severity = signal('');
  sink = signal('');
  cat = signal('');
  
  constructor(event?: SystemEvent) {
    if (event) {
      this.update(event);
    }
  }
  update(event: SystemEvent) {
    this.id.set(event.id);
    this.date.set(new Date(event.date));
    this.message.set(event.message);
    this.detail.set(event.detail);
    this.level.set(event.level);
    this.severity.set(event.severity);
    this.sink.set(event.sink);
    this.cat.set(event.cat);
  }
}