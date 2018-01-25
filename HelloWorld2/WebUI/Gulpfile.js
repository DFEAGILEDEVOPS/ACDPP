var gulp = require("gulp"),
clean = require('gulp-clean'),
sass = require("gulp-sass");

gulp.task("clean", function() {
    return gulp.src(
        ["sassobj", "wwwroot"], {read: false})
     .pipe(clean());
});

gulp.task("sass-elements", ["clean"], function() {
    return gulp.src([
        'node_modules/govuk-elements-sass/public/sass/**/*',
        'node_modules/govuk_frontend_toolkit/stylesheets/**/*',
        'node_modules/govuk_template_mustache/assets/stylesheets/**/*',
        'Styles/**/*'])
        .pipe(gulp.dest('sassobj'))
});

gulp.task("sass-frontend-images", ["clean"], function() {
    return gulp.src([
        
    ])
})

gulp.task("sass-compile", ["clean", "sass-elements"], function () {
    return gulp.src('sassobj/*.scss')
      .pipe(sass())
      .pipe(gulp.dest('wwwroot/css'));
});

gulp.task("css-copy", ["clean", "sass-elements"], function() {
    return gulp.src('sassobj/*.css')
      .pipe(gulp.dest('wwwroot/css'));
})

gulp.task("css-image-copy", ["clean", "sass-elements"], function() {
    return gulp.src('sassobj/images/**/*')
      .pipe(gulp.dest('wwwroot/css/images'));
})

gulp.task("css-font-copy", ["clean", "sass-elements"], function() {
    return gulp.src('sassobj/fonts/**/*')
      .pipe(gulp.dest('wwwroot/css/fonts'));
})

gulp.task("images-copy", ["clean"], function() {
    return gulp.src([
        'node_modules/govuk_template_mustache/assets/images/**/*',
        'node_modules/govuk_frontend_toolkit/images/**/*'
    ]).pipe(gulp.dest("wwwroot/images"))
    .pipe(gulp.dest("wwwroot/css/images")) //todo is this really necessary?!
})

gulp.task("javascripts-copy", ["clean"], function() {
    return gulp.src([
        "node_modules/govuk_template_mustache/assets/javascripts/**/*",
        "node_modules/govuk_frontend_toolkit/javascripts/**/*"
    ]).pipe(gulp.dest("wwwroot/javascripts"))
})


gulp.task("favicon-copy", ["clean"], function() {
    return gulp.src([
        "node_modules/govuk_template_mustache/assets/images/favicon.ico"
    ]).pipe(gulp.dest("wwwroot"))
})


gulp.task("default", ["sass-compile", "css-copy", "css-image-copy", "css-font-copy", "images-copy", "javascripts-copy", "favicon-copy"])