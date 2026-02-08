import 'reflect-metadata';
import {signal} from "@angular/core";
import {DiagProcess} from "@domain/DiagProcess";


export class DiagProcessSignals {
  id = signal('');
  siteId = signal('');
  instanceId = signal('');
  processName = signal('');
  userName = signal('');
  lastOnline = signal(new Date());
  isOnline = signal(false);
  machineName = signal('');

  constructor(process?: DiagProcess) {
    if (process) {
      this.update(process);
    }
  }

  update(process: DiagProcess) {
    this.id.set(process.id);
    this.siteId.set(process.siteId);
    this.instanceId.set(process.instanceId);
    this.processName.set(process.processName);
    this.userName.set(process.userName);
    this.lastOnline.set(new Date(process.lastOnline));
    this.isOnline.set(process.isOnline);
    this.machineName.set(process.machineName);
  }
}

