import { Component, ViewChild } from '@angular/core';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiMenu.component.html',
	styleUrls: ['../../app.component.css']
})
export class CoreApiMenuComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;

	constructor(
		private webapi: WebApiService,
	) {
	}

	public CreateFamiliesCollection() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/familiesContainer/create', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.alertMessage.Show("Done", result.data);
		});
	}

	public ProcessConflictsFeed() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/core/conflicts`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.alertMessage.Show("Done", result.data);
		});
	}

	private StartBusy() {
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}
