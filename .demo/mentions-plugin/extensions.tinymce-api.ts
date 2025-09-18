import { UmbTinyMcePluginBase } from '@tiny-mce-umbraco/backoffice/core';
import type { TinyMcePluginArguments } from '@tiny-mce-umbraco/backoffice/core';
import type { Editor } from '@tiny-mce-umbraco/backoffice/external/tinymce';
import type { UmbPropertyEditorConfigCollection } from '@umbraco-cms/backoffice/property-editor';
import { createMentionsRequest } from './mentions.request-factory';

export default class TinyMceMentionsExtensionApi extends UmbTinyMcePluginBase {
	readonly #editor: Editor;
	readonly #configuration: UmbPropertyEditorConfigCollection | undefined;

	constructor(args: TinyMcePluginArguments) {
		super(args);
		console.log("tinymce-extensions initialized");

		this.#editor = args.editor;
		this.#configuration = args.host.configuration;

		if (this.#editor) {
			// see all of the settings associated with the editor
			console.log("tinymce-extensions this.#editor", [this.#editor]);
		}
		if (this.#configuration) {
			// see all of the configuration settings
			console.log("tinymce-extensions this.#configuration", [this.#configuration]);
		}
	}

	static override async extendEditorConfig(_config: any): Promise<void> {
		// see the config before we plugin in the mentions
		console.log("tinymce-extensions initial config", [_config]);
		_config.mentions_fetch = await createMentionsRequest(); // or a function that returns the request handler
		// see the config after the mentions are fetched
		console.log("tinymce-extensions config with mentions", [_config]);
	}

	override async init(): Promise<void> {
		// initialize the plugin
		console.log("tinymce-extensions init");
	}
}
