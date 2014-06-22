
Wallpaperr - automatic wallpaper changer

Author:  Ian Good
email:   igood@digipen.edu
Version: 3.2.x
Date:    September 26, 2010


I. System Requirements
	OS: Windows XP/Vista/7
	Disk Space: 3.24MB (exe) + 10MB (bmp)


II. Installation
	No installation necessary. Settings and bitmap files are stored in the
	user's Application Data store.


III. License
	Wallpaperr is freeware and is provided as-is with no warranty.
	You may use this software at your own risk. The author is not
	responsible for any damage caused by this software.
	Wallpaperr may be freely distributed, provided the distribution package
	is not modified.
	No person or company may charge a fee for the distribution of
	Wallpaperr without written permission from the author.
	By using the application you have accepted the terms of this EULA.


IV. Use
	Upon starting the application, Wallpaperr runs in the system tool tray.
	To quit the application, select Exit from the tray icon menu or
		File->Exit from the menu strip. Wallpaperr closes to the tray by
		default.
	Right-click the tray icon to access the tray icon menu. Double-click
		the tray icon to open the settings window.
	File->Show On Startup may be toggled to choose if the application
		starts visible or minimized to the system tool tray.
	Layout Style determines the look of the background.
		Zoom Out shows the entire source image on a background color with a
			margin on two sides.
		Zoom In shows the source image scaled to fit the screen with no
			empty space.
		Spiffy combines the other two settings to compose an image with a
			centered foreground image surrounded by a black and white
			border over a blurred background. Very nice.
	Multiple Displays is an option available to users with more than one
		display connected to their PC. The application periodically checks
		for more than one display.
	Automatic Changes determine the interval at which new backgrounds are
		composed. Timed changes may be toggled via the tray icon menu. The
		internal timer is reset each time the application is run or a new
		background is composed.
	Image Collection shows what files and folders are available to
		Wallpaperr.
		Items may be added to the collection via drag-n-drop or by using
		the menu strip selections and buttons.
		Activating an item in the collection (double-clicking with the left
		mouse button or hitting the Enter key with an item selected) will
		cause Wallpaperr to compose a new background using that selection.
		This works for files and folders alike.
		Folders included in the collection are monitored for changes. If a
		folder or file in the collection cannot be found upon request by
		Wallpaperr, it is removed from the Image Collection.
		Selections may be right-clicked to display a context menu.
		Collections may be saved and loaded via the File menu.
	The entire list of files in the collection can be accessed via the
		Collection->View Master File List menu selection. Activating an
		item in the Master File List will cause Wallpaperr to compose a new
		background using that selection.
		Selections may be right-clicked to display a context menu.
	Image Collections may be saved & loaded via the Collection menu.
	The OK button saves the current settings and closes the application to
		the tray.
	Pressing Esc or the Cancel button restores previous settings and closes
		the application to the tray.
	The Apply button saves the current settings and composes a new
		background using the new settings.
		

V. Notes
	To run Wallpaperr on Startup, place a shortcut to the executable in the
		Startup folder in your Windows Start Menu.
	Wallpaperr does not transfer settings between versions. Upgrading to a
		new version requires the user to manually copy their user.config
		file to the new version's Application Data directory if they wish
		to keep their old settings. There is no guarantee old settings
		files are compatible but it is a safe assumption.


VI. Version History
	3.2.2
	 * fixed a crash involving blur operations writing out of bounds
	3.2.1
	 * dragging an image onto the style preview will generate a new background
	 * accepts file names as command line args for new backgrounds
	3.2
	 * added background color blend to Spiffy mode
	 * added new folder broswer
	3.1.0
	 * attempting to run a second instance now shows first instance
		even if closed to tray
	 * added Options menu dropdown with Double-Click Tray Icon menu item
	 * updated Collection sorting--alphabetical with folders first
	 * added thumbnail view in context menu
	 * rearranged UI to maximize Image Collection space
	 * added more menu icons
	3.0.7
	 * tray icons show paused/busy status
	 * fixed Pause bug (checked status not consistent with actual state)
	3.0.6
	 * fixed loading Collection from XML--also changed file layout
	 * Collection files may be added to Image Collection just like images
	 * added error handling for Collection files
	 * Ctrl+A works in Image Collection
	 * added error handling for corrupt images
	3.0.5
	 * added context menu to Image Collection items
	 * changed tray icon menu to show "Pause"/"Unpause" with images instead
		of a check mark
	 * Help/F1 opens a browser with a link to the readme
	 * added Open/Save Collection
	 * pressing Esc closes the form
	3.0.4
	 * removed annoying Vista settings message
	 * added periodic checking for single-/multi-display
	 * fixed crash bug when trying to add file/folder that does not exist
	 * changed tray icon double-click to show settings
	3.0.3
	 * added threaded blurring for Spiffy setting--major performance
		increase on dev machine
	 * added double-click functionality for folders in Image Collection
	 * added notifier messages for empty folders
	 * updated folder watchers--proper disposal
	3.0.2
	 * added threaded image composition--for multiple displays, each image
		is composed on its own thread
	 * updated internal lists to use generics (preferred method)
	3.0.1
	 * updated [read:fixed] multiple display composition
	 * updated progress bar status updates
	 * updated folder watcher reaction time
	3.0.0
	 * updated background composition to use Background Worker--form
		remains responsize while working
	 * added folder watchers
	 * added show/hide on startup
	 * updated multiple display composition
	1.0.0 - 2.6.0
	 * no records kept
