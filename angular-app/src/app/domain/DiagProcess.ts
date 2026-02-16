export interface DiagProcess {
    id: number;
    siteId: number;
    instanceId: string;
    name: string;
    userName: string;
    lastOnline: string | Date;
    isOnline: boolean;
    machineName: string;
}