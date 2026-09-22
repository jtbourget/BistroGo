// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


// Highlight the active sidebar link based on current URL

    
        document.addEventListener('DOMContentLoaded', function () 
        {const current = window.location.pathname.toLowerCase();
    
        document.querySelectorAll('.sidebar-nav .nav-link').forEach(link => 
            {const href = link.getAttribute('href')?.toLowerCase() || '';
        
                if (href && (current === href || current.startsWith(href + '/'))) 
                    {
                        link.classList.add('active');
                    }
            });
        });