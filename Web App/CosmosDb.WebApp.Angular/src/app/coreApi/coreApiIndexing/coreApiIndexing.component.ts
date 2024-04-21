import { Component, ViewChild } from '@angular/core';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiIndexing.component.html',
	styleUrls: [
		'./coreApiIndexing.component.css',
		'../../app.component.css',
	]
})
export class CoreApiIndexingComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public document: any;

	constructor(
		public formatter: FormatterService,
		private webapi: WebApiService,
	) {
	}

	public AutomaticIndexing() {
		this.CallServer('/api/core/indexing/automatic');
	}

	public ManualIndexing() {
		this.CallServer('/api/core/indexing/manual');
	}

	public PathIndexing() {
		this.CallServer('/api/core/indexing/path');
	}

	private CallServer(url: string) {
		this.StartBusy();
		this.webapi.InvokeGetText(url, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	private StartBusy() {
		this.document = null;
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}
