export interface DiagProcess {
    id: string;
    siteId: string;
    instanceId: string;
    processName: string;
    userName: string;
    lastOnline: string | Date;
    isOnline: boolean;
    machineName: string;
}