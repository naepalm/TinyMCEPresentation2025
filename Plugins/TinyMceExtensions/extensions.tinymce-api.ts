import { UmbTinyMcePluginBase } from '@tiny-mce-umbraco/backoffice/core';
import type { TinyMcePluginArguments } from '@tiny-mce-umbraco/backoffice/core';
import type { Editor } from '@tiny-mce-umbraco/backoffice/external/tinymce';
import type { UmbPropertyEditorConfigCollection } from '@umbraco-cms/backoffice/property-editor';
import { createMentionsRequest } from './mentions.request-factory';
import { createMentionsSelect } from './mentions.select-factory';


export default class TinyMceMentionsExtensionApi extends UmbTinyMcePluginBase {
	readonly #editor: Editor;
	readonly #configuration: UmbPropertyEditorConfigCollection | undefined;

	constructor(args: TinyMcePluginArguments) {
		super(args);
		console.log("mentions-plugin initialized");

		this.#editor = args.editor;
		this.#configuration = args.host.configuration;

		if (this.#editor) {
			// see all of the settings associated with the editor
			console.log("mentions-plugin this.#editor", [this.#editor]);
		}
		if (this.#configuration) {
			// see all of the configuration settings
			console.log("mentions-plugin this.#configuration", [this.#configuration]);
		}
	}
	
	static override async extendEditorConfig(_config: any): Promise<void> {
		// see the config before we plugin in the mentions
		console.log("mentions-plugin initial config", [_config]);
		_config.mentions_fetch = await createMentionsRequest(); // or a function that returns the request handler
        _config.mentions_menu_hover = await createMentionsSelect();
		// see the config after the mentions are fetched
		console.log("mentions-plugin config with mentions", [_config]);
	}

	override async init(): Promise<void> {
		// initialize the plugin
		console.log("mentions-plugin init");
	}
}
