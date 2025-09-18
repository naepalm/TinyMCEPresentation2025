import { UmbTinyMcePluginBase } from '@tiny-mce-umbraco/backoffice/core';
import type { TinyMcePluginArguments } from '@tiny-mce-umbraco/backoffice/core';
import type { Editor } from '@tiny-mce-umbraco/backoffice/external/tinymce';
import type { UmbPropertyEditorConfigCollection } from '@umbraco-cms/backoffice/property-editor';
import { createMentionsRequest } from './mentions.request-factory';
import { createMentionsSelect } from './mentions.select-factory';
import { createAdvTemplatesRequest } from './advtemplates.request-factory';
import { createAdvTemplatesSelect } from './advtemplates.select-factory';
import { createMergeTagsRequest } from './mergetags.request-factory';

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
        _config.mentions_menu_hover = await createMentionsSelect();
		_config.advtemplate_list = await createAdvTemplatesRequest();
		_config.advtemplate_get_template = await createAdvTemplatesSelect();

		_config.mergetags_prefix = "[";
		_config.mergetags_suffix = "]";
		_config.mergetags_list = await createMergeTagsRequest();

		// see the config after the mentions are fetched
		console.log("tinymce-extensions config with mentions", [_config]);
	}

	override async init(): Promise<void> {
		// initialize the plugin
		console.log("tinymce-extensions init");
	}
}
