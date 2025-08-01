import 'reflect-metadata';
import * as _ from 'lodash-es';
import {OnlineState} from './OnlineState';
import {Level} from './Level';

export class DiagProcess {
    id: string = '';
    processName: string = '';
    processId: number = 0;
    instanceName: string = '';
    machineName: string = '';
    userName: string = '';
    uri: string = '';
    message: string = '';
    alertLevel = 0;
    labelClass = '';

    lastOnline: Date | string | undefined = undefined;
    state: OnlineState = 'NA';
    connectionId: string = '';


    constructor(data?: DiagProcess) {
        if (data)
            this.update(data);
    }

    get title(): string {
        return `${this.machineName}/${this.userName}/${this.processName}`;
    }

    update(s: DiagProcess): void {
        _.assign(this, s);
        //console.log(`${this.id} state: ${this.state}`)
        this.labelClass = this.alertLevel === 0 ? '' : 'event-level-' + Level.LevelToString(this.alertLevel).toLocaleLowerCase();
    }
}

