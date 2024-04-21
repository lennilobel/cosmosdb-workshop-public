import { Injectable } from '@angular/core';
import { JsonPipe } from '@angular/common';

@Injectable()
export class FormatterService {

	constructor(
		private jsonPipe: JsonPipe
	) {
	}

	public AsJson(obj: any): string {
		let json = JSON.stringify(obj);
		let c = json.substr(0, 1);
		if (c != '{' && c != '[') {
			return obj;
		}

		json = this.jsonPipe.transform(JSON.parse(json));

		return json;
	}
	
}
