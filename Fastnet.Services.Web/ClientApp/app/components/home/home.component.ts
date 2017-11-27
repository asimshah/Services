import { Component } from '@angular/core';
//import { Component, OnInit } from '@angular/core';

//import { BackupService, SourceFolder, SourceType} from '../shared/backup.service';

@Component({
    selector: 'home',
    templateUrl: './home.component.html',
    styleUrls:['./home.component.scss']
})
export class HomeComponent {

}
//export class HomeComponent implements OnInit {
//    sources: SourceFolder[];
//    ready: boolean;
//    constructor(private backupService: BackupService) {
//        this.ready = false;
//    }
//    async ngOnInit() {
//        this.sources = await this.backupService.getEnabledSources();
//        this.ready = true;
//    }
//    getFormattedDate2(d: string): string {
//        let date = new Date(d);
//        let options: Intl.DateTimeFormatOptions = {day: "2-digit", month: "short", year: "2-digit", hour: "2-digit", minute: "2-digit" }
//        return new Intl.DateTimeFormat("en-GB", options).format(date);
//    }
//    getFormattedDate(d: Date): string {
//        if (d) {
//            var monthNames = [
//                "January", "February", "March",
//                "April", "May", "June", "July",
//                "August", "September", "October",
//                "November", "December"
//            ];
//            var day = d.getDate();
//            var monthIndex = d.getMonth();
//            var year = d.getFullYear();
//            let formattedDay = day.toString();
//            if (day < 10) {
//                formattedDay = "0" + formattedDay;
//            }
//            return formattedDay + monthNames[monthIndex].substr(0, 3) + year.toString().substr(2)
//                + ' ' + d.getHours() + ':' + d.getMinutes();
//        }
//        else {
//            return "";
//        }
//    }
//}
