import { Component, OnInit, ViewChild } from '@angular/core';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { FormatterService } from 'src/app/shared/services/formatter.service';

@Component({
	templateUrl: './CoreApiConflicts.component.html',
	styleUrls: [
		'./CoreApiConflicts.component.css',
		'../../app.component.css',
	]
})
export class CoreApiConflictsComponent implements OnInit {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public gridColumns!: any[] | null;
	public gridData!: any[] | null;
	public conflictProps: any;
	public conflict: any;

	constructor(
		private webapi: WebApiService,
		public formatter: FormatterService,
	) {
	}

	public ngOnInit() {
	}

	public ViewConflicts() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/conflicts', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.gridColumns = ['ConflictId', 'OperationType', 'ResourceId', 'ResourceType', 'DocumentId', 'PartitionKey', 'Open'];
			this.gridData = result.data;
		});
	}

	public GetConflict(conflictProps: any) {
		this.StartBusy();
		this.webapi.InvokePost(`/api/core/conflict`, conflictProps, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.conflictProps = conflictProps;
			this.conflict = result.data;
		});
	}

	public KeepWinner() {
		this.StartBusy();
		this.webapi.InvokePost(`/api/core/conflict/resolve/winner`, this.conflictProps, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.ViewConflicts();
		});
	}

	public KeepLoser() {
		this.StartBusy();
		this.webapi.InvokePost(`/api/core/conflict/resolve/loser`, this.conflictProps, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.ViewConflicts();
		});
	}

	private StartBusy() {
		this.busyIndex++;
	}

	private EndBusy() {
		this.conflict = null;
		this.gridColumns = null;
		this.gridData = null;
		this.busyIndex--;
	}

}
