import {Secret} from "./Secret";

export interface Site {
    
    id: string,
    code: string,
    name: string,
    secrets: Secret[];
}

