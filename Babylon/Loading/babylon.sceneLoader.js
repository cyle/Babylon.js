﻿var BABYLON;
(function (BABYLON) {
    var SceneLoader = (function () {
        function SceneLoader() {
        }
        Object.defineProperty(SceneLoader, "ForceFullSceneLoadingForIncremental", {
            get: function () {
                return SceneLoader._ForceFullSceneLoadingForIncremental;
            },
            set: function (value) {
                SceneLoader._ForceFullSceneLoadingForIncremental = value;
            },
            enumerable: true,
            configurable: true
        });


        SceneLoader._getPluginForFilename = function (sceneFilename) {
            var dotPosition = sceneFilename.lastIndexOf(".");
            var extension = sceneFilename.substring(dotPosition).toLowerCase();

            for (var index = 0; index < this._registeredPlugins.length; index++) {
                var plugin = this._registeredPlugins[index];

                if (plugin.extensions.indexOf(extension) !== -1) {
                    return plugin;
                }
            }

            return this._registeredPlugins[this._registeredPlugins.length - 1];
        };

        // Public functions
        SceneLoader.RegisterPlugin = function (plugin) {
            plugin.extensions = plugin.extensions.toLowerCase();
            SceneLoader._registeredPlugins.push(plugin);
        };

        SceneLoader.ImportMesh = function (meshesNames, rootUrl, sceneFilename, scene, onsuccess, progressCallBack, onerror) {
            var _this = this;
            var manifestChecked = function (success) {
                scene.database = database;

                var plugin = _this._getPluginForFilename(sceneFilename);

                var importMeshFromData = function (data) {
                    var meshes = [];
                    var particleSystems = [];
                    var skeletons = [];

                    try  {
                        if (!plugin.importMesh(meshesNames, scene, data, rootUrl, meshes, particleSystems, skeletons)) {
                            if (onerror) {
                                onerror(scene);
                            }

                            return;
                        }
                    } catch (e) {
                        if (onerror) {
                            onerror(scene);
                        }

                        return;
                    }

                    if (onsuccess) {
                        scene.importedMeshesFiles.push(rootUrl + sceneFilename);
                        onsuccess(meshes, particleSystems, skeletons);
                    }
                };

                if (sceneFilename.substr && sceneFilename.substr(0, 5) === "data:") {
                    // Direct load
                    importMeshFromData(sceneFilename.substr(5));
                    return;
                }

                BABYLON.Tools.LoadFile(rootUrl + sceneFilename, function (data) {
                    importMeshFromData(data);
                }, progressCallBack, database);
            };

            // Checking if a manifest file has been set for this scene and if offline mode has been requested
            var database = new BABYLON.Database(rootUrl + sceneFilename, manifestChecked);
        };

        /**
        * Load a scene
        * @param rootUrl a string that defines the root url for scene and resources
        * @param sceneFilename a string that defines the name of the scene file. can start with "data:" following by the stringified version of the scene
        * @param engine is the instance of BABYLON.Engine to use to create the scene
        */
        SceneLoader.Load = function (rootUrl, sceneFilename, engine, onsuccess, progressCallBack, onerror, showLoadingScreen) {
            if (typeof showLoadingScreen === "undefined") { showLoadingScreen = true; }
            SceneLoader.Append(rootUrl, sceneFilename, new BABYLON.Scene(engine), onsuccess, progressCallBack, onerror, showLoadingScreen);
        };

        /**
        * Append a scene
        * @param rootUrl a string that defines the root url for scene and resources
        * @param sceneFilename a string that defines the name of the scene file. can start with "data:" following by the stringified version of the scene
        * @param scene is the instance of BABYLON.Scene to append to
        */
        SceneLoader.Append = function (rootUrl, sceneFilename, scene, onsuccess, progressCallBack, onerror, showLoadingScreen) {
            if (typeof showLoadingScreen === "undefined") { showLoadingScreen = true; }
            var plugin = this._getPluginForFilename(sceneFilename.name || sceneFilename);
            var database;

            if (showLoadingScreen) {
                scene.getEngine().displayLoadingUI();
            }

            var loadSceneFromData = function (data) {
                scene.database = database;

                if (!plugin.load(scene, data, rootUrl)) {
                    if (onerror) {
                        onerror(scene);
                    }

                    scene.getEngine().hideLoadingUI();
                    return;
                }

                if (onsuccess) {
                    onsuccess(scene);
                }

                if (showLoadingScreen) {
                    scene.executeWhenReady(function () {
                        scene.getEngine().hideLoadingUI();
                    });
                }
            };

            var manifestChecked = function (success) {
                BABYLON.Tools.LoadFile(rootUrl + sceneFilename, loadSceneFromData, progressCallBack, database);
            };

            if (sceneFilename.substr && sceneFilename.substr(0, 5) === "data:") {
                // Direct load
                loadSceneFromData(sceneFilename.substr(5));
                return;
            }

            if (rootUrl.indexOf("file:") === -1) {
                // Checking if a manifest file has been set for this scene and if offline mode has been requested
                database = new BABYLON.Database(rootUrl + sceneFilename, manifestChecked);
            } else {
                BABYLON.Tools.ReadFile(sceneFilename, loadSceneFromData, progressCallBack);
            }
        };
        SceneLoader._ForceFullSceneLoadingForIncremental = false;

        SceneLoader._registeredPlugins = new Array();
        return SceneLoader;
    })();
    BABYLON.SceneLoader = SceneLoader;
    ;
})(BABYLON || (BABYLON = {}));
//# sourceMappingURL=babylon.sceneLoader.js.map
