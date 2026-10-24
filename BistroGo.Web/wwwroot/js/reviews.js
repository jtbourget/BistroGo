// Customer feedback and review interactions

document.addEventListener('DOMContentLoaded', function () {
    const reviewForm = document.getElementById('reviewForm');
    reviewForm?.addEventListener('submit', function (e) {
        e.preventDefault();
    });

    const stars = document.querySelectorAll('#starRating .star-btn');
    const ratingText = document.getElementById('ratingText');
    const ratingInput = document.getElementById('ratingValue');
    const reviewComment = document.getElementById('reviewComment');
    const charCounter = document.getElementById('charCounter');
    const submitBtn = document.getElementById('submitReviewBtn');

    // Text labels for each rating score
    const labels = {
        1: '1 Star - Needs Improvement',
        2: '2 Stars - Fair',
        3: '3 Stars - Good',
        4: '4 Stars - Very Good',
        5: '5 Stars - Excellent'
    };

    let selectedRating = 5;

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

    // Set default initial state (5 stars)
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

    // Live character counter for review textarea
    reviewComment?.addEventListener('input', function () {
        if (charCounter) {
            charCounter.textContent = `${this.value.length} / 1000`;
        }
    });

    // Submit button placeholder: prevents submission per story requirements
    submitBtn?.addEventListener('click', function (e) {
        e.preventDefault();
    });
});
