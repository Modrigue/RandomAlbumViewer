# RandomAlbumViewer
A program to randomly view photos albums by categories.

In the AVConfig.txt configuration file:
- Set the albums root directory,
- Set the slideshow interval (in ms, default is 6000),
- In the [ALBUMS] section, set the albums directories (relative to the root directory).
  An album can contain more than 1 directories, they must be separated by ';'.

Commands during diaporama:
- Mouse left button / '->'    : goto next random directory of the selected album
- Mouse right button / 'ESC'  : back to menu
- Mouse wheel down / "PgDown" : goto next image
- Mouse wheel up / "PgUp"     : goto previous image
- Mouse middle button / 'P'   : pause on current image
- 'Home'                      : goto first image of the current directory
- 'End'                       : goto last image of the current directory
- 'Delete'                    : deletes current image
- 'Space' / 'B'               : close viewer

Supported image formats:
- JPG, JPG2000,
- BMP,
- PNG,
- TIFF,
- GIF
