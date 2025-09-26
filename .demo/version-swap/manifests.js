const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;

(function loadTinyMce() {
  const script1 = document.createElement("script");
  script1.src = `https://cdn.tiny.cloud/1/${apiKey}/tinymce/6/tinymce.min.js`;
  script1.referrerPolicy = "origin";
  script1.async = true;
  document.head.appendChild(script1);

  const script2 = document.createElement("script");
  script2.src = `https://cdn.tiny.cloud/1/${apiKey}/tinymce/6/icons/default/icons.min.js`;
  script2.referrerPolicy = "origin";
  script2.async = true;
  document.head.appendChild(script2);
})();
