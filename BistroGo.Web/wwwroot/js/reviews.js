// Customer feedback and review interactions

document.addEventListener('DOMContentLoaded', function () {
    const reviewForm = document.getElementById('reviewForm');
    const stars = document.querySelectorAll('#starRating .star-btn');
    const ratingText = document.getElementById('ratingText');
    const ratingInput = document.getElementById('ratingValue');
    const reviewComment = document.getElementById('reviewComment');
    const charCounter = document.getElementById('charCounter');

    // Format all review and comment timestamps into the user's browser local time
    document.querySelectorAll('time.review-timestamp[datetime]').forEach(el => {
        const iso = el.getAttribute('datetime');
        if (!iso) return;
        const date = new Date(iso);
        if (!isNaN(date.getTime())) {
            el.textContent = date.toLocaleDateString(undefined, {
                month: 'short',
                day: 'numeric',
                year: 'numeric'
            }) + ' ' + date.toLocaleTimeString(undefined, {
                hour: 'numeric',
                minute: '2-digit'
            });
        }
    });

    // Text labels for each rating score
    const labels = {
        1: '1 Star - Needs Improvement',
        2: '2 Stars - Fair',
        3: '3 Stars - Good',
        4: '4 Stars - Very Good',
        5: '5 Stars - Excellent'
    };

    let selectedRating = ratingInput && ratingInput.value ? parseInt(ratingInput.value, 10) : 5;

    // Update star highlights and descriptive text
    function setStars(rating) {
        stars.forEach(star => {
            const starVal = parseInt(star.getAttribute('data-rating'), 10);
            star.classList.toggle('active', starVal <= rating);
        });
        if (ratingText) {
            ratingText.textContent = labels[rating] || (rating + ' Stars');
        }
    }

    // Set default initial state
    if (stars.length > 0) {
        setStars(selectedRating);

        // Add hover and click listeners to each star button
        stars.forEach(star => {
            const starVal = parseInt(star.getAttribute('data-rating'), 10);

            star.addEventListener('mouseenter', function () {
                setStars(starVal);
            });

            star.addEventListener('click', function () {
                selectedRating = starVal;
                if (ratingInput) {
                    ratingInput.value = selectedRating;
                }
                setStars(selectedRating);
            });
        });

        // Reset star highlights back to selected value on mouse leave
        document.getElementById('starRating')?.addEventListener('mouseleave', function () {
            setStars(selectedRating);
        });
    }

    // Live character counter for review textarea
    if (reviewComment) {
        if (charCounter) {
            charCounter.textContent = `${reviewComment.value.length} / 1000`;
        }
        reviewComment.addEventListener('input', function () {
            if (charCounter) {
                charCounter.textContent = `${this.value.length} / 1000`;
            }
        });
    }

    // Client-side validation before submission
    reviewForm?.addEventListener('submit', function (e) {
        if (ratingInput) {
            ratingInput.value = selectedRating;
        }

        if (reviewComment && reviewComment.value.trim().length === 0) {
            e.preventDefault();
            reviewComment.focus();
            alert('Please enter your written feedback before submitting.');
        }
    });
});
