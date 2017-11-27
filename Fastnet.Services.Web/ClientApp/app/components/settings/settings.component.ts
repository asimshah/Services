import { Component, OnInit } from '@angular/core';

import { BackupService, SourceFolder, SourceType} from '../shared/backup.service';

@Component({
    selector: 'home',
    templateUrl: './settings.component.html',
    styleUrls:['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
    sources: SourceFolder[];
    ready: boolean;
    constructor(private backupService: BackupService) {
        this.ready = false;
    }
    async ngOnInit() {
        this.sources = await this.backupService.getSources();
        this.ready = true;
    }
    async onBackupEnabledChanged(sf: SourceFolder) {
        console.log(`${sf.displayName} backup enabled changed`);
        await this.backupService.setBackupEnabled(sf.id, sf.backupEnabled);
    }
    async reconfigure() {
        await this.backupService.reconfigureSettings();
    }
}
