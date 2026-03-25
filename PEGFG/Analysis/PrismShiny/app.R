.prism_app_dir <- tryCatch({
  normalizePath(dirname(sys.frame(1)$ofile), winslash = '/', mustWork = FALSE)
}, error = function(e) {
  normalizePath(getwd(), winslash = '/', mustWork = FALSE)
})
source(file.path(.prism_app_dir, 'app_main.R'), local = TRUE, encoding = 'UTF-8')
shinyApp(ui, server)
