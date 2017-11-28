import { Injectable } from '@angular/core';
import { Http } from '@angular/http';

import { BaseService } from './base.service';

export enum BackupState {
    NotStarted,
    Started,
    Finished,
    Failed
}
export enum SourceType {
    Folder,
    Website
}
export class SourceFolder {
    id: number;
    displayName: string;
    path: string;
    fullPath: string;
    backupEnabled: boolean;
    type: SourceType;
    backups: Backup[]
}
export class Backup {
    id: number;
    scheduledOn: Date;
    backedUpOn: Date;
    state: BackupState;
    fullPath: string;
    sourceFolderid: number;
}
export class BackupDestinationInfo {
    volumeLabel: string;
    available: boolean;
    destination: string;
}

@Injectable()
export class BackupService extends BaseService {
    constructor(http: Http) {
        super(http);
    }
    async getSources(): Promise<SourceFolder[]> {
        let query = 'backup/get/sources';
        return this.stdGetDataResultQueryWithoutNull<SourceFolder[]>(query);
    }
    async getEnabledSources(): Promise<SourceFolder[]> {
        let query = 'backup/get/enabled/sources';
        return this.stdGetDataResultQueryWithoutNull<SourceFolder[]>(query);
    }
    async setBackupEnabled(id: number, enabled: boolean): Promise<void> {
        let query = `backup/set/source/${id}/${enabled}`;
        return this.stdGetDataResultQueryWithoutNull<void>(query);
    }
    async reconfigureSettings() {
        let query = `backup/reconfigure`;
        return this.stdGetDataResultQuery<void>(query);
    }
    async getBackupDestinationStatus(): Promise<boolean> {
        let query = `backup/get/backup/destinationStatus`;
        return this.stdGetDataResultQueryWithoutNull<boolean>(query);
    }
    async getBackupDestination(): Promise<BackupDestinationInfo> {
        let query = `backup/get/backup/destination`;
        return this.stdGetDataResultQueryWithoutNull<BackupDestinationInfo>(query);
    }
    private async stdGetDataResultQuery<T>(query: string): Promise<T | null> {
        let result = await this.query(query);
        if (!result.success) {
            return new Promise<null>(resolve => resolve(null));
        }
        else {
            return new Promise<T>(resolve => resolve(<T>result.data));
        }
    }
    private async stdGetDataResultQueryWithoutNull<T>(query: string): Promise<T> {
        let result = await this.query(query);
        if (!result.success) {
            throw `stdGetDataResultQueryWithoutNull cannot return null`
        }
        else {
            //console.log(`${JSON.stringify(result.data)}`);
            return new Promise<T>(resolve => resolve(<T>result.data));
        }
    }
}
